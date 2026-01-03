import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Header, Label } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import * as Yup from 'yup';
import { toast } from "react-toastify";

export default observer(function ChangePasswordForm() {
    const { userStore } = useStore();

    const validationSchema = Yup.object({
        currentPassword: Yup.string().required('Current password is required'),
        newPassword: Yup.string().required('New password is required').min(12, 'Password must be at least 12 characters'),
        confirmPassword: Yup.string()
            .required('Please confirm your password')
            .oneOf([Yup.ref('newPassword')], 'Passwords must match')
    });

    return (
        <Formik
            initialValues={{ currentPassword: '', newPassword: '', confirmPassword: '', error: null }}
            validationSchema={validationSchema}
            onSubmit={(values, { setErrors, resetForm }) => userStore.changePassword({
                currentPassword: values.currentPassword,
                newPassword: values.newPassword
            }).then(() => {
                toast.success('Password changed successfully');
                resetForm();
                window.location.href = '/invoices';
            }).catch(error =>
                setErrors({ error: 'Failed to change password. Please check your current password.' }))}
        >
            {({ handleSubmit, isSubmitting, errors, handleChange, values, touched }) => (
                <FormikForm className='ui form' onSubmit={handleSubmit} autoComplete='off'>
                    <Header as='h2' content='Change Password' color='teal' textAlign='center' />
                    <Form.Input
                        name='currentPassword'
                        placeholder='Current Password'
                        type='password'
                        value={values.currentPassword}
                        onChange={handleChange}
                        error={touched.currentPassword && errors.currentPassword}
                    />
                    <ErrorMessage name='currentPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

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
                    <Button loading={isSubmitting} positive content='Change Password' type='submit' fluid />
                </FormikForm>
            )}
        </Formik>
    )
})
