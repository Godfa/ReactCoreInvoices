import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Header, Label, Message } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import * as Yup from 'yup';
import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";

export default observer(function ResetPasswordForm() {
    const { userStore } = useStore();
    const [searchParams] = useSearchParams();
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const email = searchParams.get('email') || '';
    const token = searchParams.get('token') || '';

    const validationSchema = Yup.object({
        newPassword: Yup.string().required('New password is required').min(12, 'Password must be at least 12 characters'),
        confirmPassword: Yup.string()
            .required('Please confirm your password')
            .oneOf([Yup.ref('newPassword')], 'Passwords must match')
    });

    if (!email || !token) {
        return (
            <div className='ui form'>
                <Header as='h2' content='Reset Password' color='teal' textAlign='center' />
                <Message negative>
                    <Message.Header>Invalid Reset Link</Message.Header>
                    <p>This password reset link is invalid or has expired.</p>
                    <p><Link to='/forgot-password'>Request a new password reset link</Link></p>
                </Message>
            </div>
        );
    }

    return (
        <Formik
            initialValues={{ newPassword: '', confirmPassword: '', error: null }}
            validationSchema={validationSchema}
            onSubmit={(values, { setErrors, setSubmitting }) => userStore.resetPassword({
                email,
                token,
                newPassword: values.newPassword
            }).then((message) => {
                setSuccessMessage(message);
                setSubmitting(false);
            }).catch(error => {
                setErrors({ error: 'Failed to reset password. The link may have expired or is invalid.' });
                setSubmitting(false);
            })}
        >
            {({ handleSubmit, isSubmitting, errors, handleChange, values, touched }) => (
                <FormikForm className='ui form' onSubmit={handleSubmit} autoComplete='off'>
                    <Header as='h2' content='Reset Password' color='teal' textAlign='center' />

                    {successMessage ? (
                        <Message positive>
                            <Message.Header>Password Reset Successfully</Message.Header>
                            <p>{successMessage}</p>
                            <p>You can now log in with your new password.</p>
                            <p><Link to='/login'>Go to login</Link></p>
                        </Message>
                    ) : (
                        <>
                            <p style={{ textAlign: 'center', marginBottom: 20 }}>
                                Enter your new password below.
                            </p>

                            <Form.Input
                                name='newPassword'
                                placeholder='New Password'
                                type='password'
                                value={values.newPassword}
                                onChange={handleChange}
                                error={touched.newPassword && errors.newPassword}
                            />
                            <ErrorMessage name='newPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                            <Form.Input
                                name='confirmPassword'
                                placeholder='Confirm New Password'
                                type='password'
                                value={values.confirmPassword}
                                onChange={handleChange}
                                error={touched.confirmPassword && errors.confirmPassword}
                            />
                            <ErrorMessage name='confirmPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                            <ErrorMessage
                                name='error' render={() =>
                                    <Label style={{ marginBottom: 10 }} basic color='red' content={errors.error} />}
                            />
                            <Button loading={isSubmitting} positive content='Reset Password' type='submit' fluid />

                            <div style={{ textAlign: 'center', marginTop: 15 }}>
                                <Link to='/login'>Back to login</Link>
                            </div>
                        </>
                    )}
                </FormikForm>
            )}
        </Formik>
    )
})
