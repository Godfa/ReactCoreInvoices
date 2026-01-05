import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Header, Label, Message } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import * as Yup from 'yup';
import { useState } from "react";
import { Link } from "react-router-dom";

export default observer(function ForgotPasswordForm() {
    const { userStore } = useStore();
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const validationSchema = Yup.object({
        email: Yup.string().required('Email is required').email('Invalid email address')
    });

    return (
        <Formik
            initialValues={{ email: '', error: null }}
            validationSchema={validationSchema}
            onSubmit={(values, { setErrors, setSubmitting }) => userStore.forgotPassword({
                email: values.email
            }).then((message) => {
                setSuccessMessage(message);
                setSubmitting(false);
            }).catch(error => {
                setErrors({ error: 'Failed to send password reset email.' });
                setSubmitting(false);
            })}
        >
            {({ handleSubmit, isSubmitting, errors, handleChange, values, touched }) => (
                <FormikForm className='ui form' onSubmit={handleSubmit} autoComplete='off'>
                    <Header as='h2' content='Forgot Password?' color='teal' textAlign='center' />

                    {successMessage ? (
                        <Message positive>
                            <Message.Header>Email Sent</Message.Header>
                            <p>{successMessage}</p>
                            <p>Please check your email inbox and follow the instructions to reset your password.</p>
                            <p><Link to='/login'>Return to login</Link></p>
                        </Message>
                    ) : (
                        <>
                            <p style={{ textAlign: 'center', marginBottom: 20 }}>
                                Enter your email address and we'll send you a link to reset your password.
                            </p>

                            <Form.Input
                                name='email'
                                placeholder='Email'
                                type='email'
                                value={values.email}
                                onChange={handleChange}
                                error={touched.email && errors.email}
                            />
                            <ErrorMessage name='email' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                            <ErrorMessage
                                name='error' render={() =>
                                    <Label style={{ marginBottom: 10 }} basic color='red' content={errors.error} />}
                            />
                            <Button loading={isSubmitting} positive content='Send Reset Link' type='submit' fluid />

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
